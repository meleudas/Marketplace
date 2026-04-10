import type {
  CompanyDto,
  CreateCompanyRequest,
  UpdateCompanyRequest,
} from "@/features/admin/model/admin.types";
import type { CompanyFormValues } from "@/features/admin/validation/company-form.schema";

export const companyDtoToFormValues = (company: CompanyDto): CompanyFormValues => {
  return {
    name: company.name,
    slug: company.slug,
    description: company.description,
    imageUrl: company.imageUrl ?? "",
    contactEmail: company.contactEmail,
    contactPhone: company.contactPhone,
    address: {
      street: company.address.street,
      city: company.address.city,
      state: company.address.state,
      postalCode: company.address.postalCode,
      country: company.address.country,
    },
    metaRaw: company.metaRaw ?? "",
  };
};

export const buildCompanyPayload = (
  form: CompanyFormValues,
): CreateCompanyRequest | UpdateCompanyRequest => {
  const imageUrl = (form.imageUrl ?? "").trim();
  const metaRaw = (form.metaRaw ?? "").trim();

  return {
    name: form.name.trim(),
    slug: form.slug.trim(),
    description: form.description.trim(),
    imageUrl: imageUrl || null,
    contactEmail: form.contactEmail.trim(),
    contactPhone: form.contactPhone.trim(),
    address: {
      street: form.address.street.trim(),
      city: form.address.city.trim(),
      state: form.address.state.trim(),
      postalCode: form.address.postalCode.trim(),
      country: form.address.country.trim(),
    },
    metaRaw: metaRaw || null,
  };
};

