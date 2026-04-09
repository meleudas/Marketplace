import type {
  CompanyDto,
  CreateCompanyRequest,
  UpdateCompanyRequest,
} from "@/features/admin/model/admin.types";

export interface CompanyFormState {
  name: string;
  slug: string;
  description: string;
  imageUrl: string;
  contactEmail: string;
  contactPhone: string;
  street: string;
  city: string;
  state: string;
  postalCode: string;
  country: string;
  metaRaw: string;
}

export const defaultCompanyFormState: CompanyFormState = {
  name: "",
  slug: "",
  description: "",
  imageUrl: "",
  contactEmail: "",
  contactPhone: "",
  street: "",
  city: "",
  state: "",
  postalCode: "",
  country: "",
  metaRaw: "",
};

export const companyDtoToFormState = (company: CompanyDto): CompanyFormState => {
  return {
    name: company.name,
    slug: company.slug,
    description: company.description,
    imageUrl: company.imageUrl ?? "",
    contactEmail: company.contactEmail,
    contactPhone: company.contactPhone,
    street: company.address.street,
    city: company.address.city,
    state: company.address.state,
    postalCode: company.address.postalCode,
    country: company.address.country,
    metaRaw: company.metaRaw ?? "",
  };
};

export const buildCompanyPayload = (
  form: CompanyFormState,
): CreateCompanyRequest | UpdateCompanyRequest => {
  return {
    name: form.name.trim(),
    slug: form.slug.trim(),
    description: form.description.trim(),
    imageUrl: form.imageUrl.trim() || null,
    contactEmail: form.contactEmail.trim(),
    contactPhone: form.contactPhone.trim(),
    address: {
      street: form.street.trim(),
      city: form.city.trim(),
      state: form.state.trim(),
      postalCode: form.postalCode.trim(),
      country: form.country.trim(),
    },
    metaRaw: form.metaRaw.trim() || null,
  };
};

